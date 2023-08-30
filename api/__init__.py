import sys
print(sys.executable)

import os
import logging
import openai
from  importlib.metadata import version
import time
import json
from datetime import datetime
from enum import IntEnum

try:
    # Cloud syntax
    from prompt_strings import parser_system_message
    from prompt_strings import answerer_system_message

except ImportError:
    # Local syntax
    from .prompt_strings import parser_system_message
    from .prompt_strings import answerer_system_message

class AISmartsLevel(IntEnum):
    gpt3 = 1
    gpt4 = 2

class GPT3Lengths(IntEnum):
    short = 1
    medium = 2
    long = 3

class GPT4Lengths(IntEnum):
    short = 1
    medium = 2
    long = 3

GPT3_Models = {
    GPT3Lengths.short: 'gpt-3.5-turbo',
    GPT3Lengths.medium: 'gpt-3.5-turbo-16k', #Comment this out to test context limit reached 'gpt-35-turbo-16K'
    # Add more as needed
}

GPT4_Models = {
    GPT4Lengths.short: 'gpt-4',
    # GPT4Lengths.medium: 'gpt-4-32K',
    # Add more as needed
}


def Print_And_Log(message: str):
        logging.info(message)
        print(message)

def Main_Loop(payload):
    Print_And_Log(f"starting main loop with snippet: {payload['text_snippet']}")

    version_openai = version('openai')
    Print_And_Log('Openai version: ' + version_openai)

    temperature = 0.5  # default value
    answerer_response_data = {}
    answerer_response_data['chat_response'] = ""
    answerer_response_data['function_response'] = ""
    answerer_response_data['user_exit'] = False

    Call_LLMs_Series(payload['text_snippet'], temperature, answerer_response_data)
    # return func.HttpResponse(f"{responseText}")
    headers = {"Content-Type": "application/json"}
    
    return_payload = {
            "user_exit": False,
            "message": answerer_response_data['chat_response']
    }
    return return_payload

parser_chat_session = None 
answerer_chat_session = None 
    
def Call_LLMs_Series(prompt: str, temperature: float, answerer_response_data: dict) -> str:
    global parser_chat_session
    global answerer_chat_session

    # openai.api_version = "" 
    #openai.api_base = ""
    openai.api_key = os.environ["openai_api_key"]

    Print_And_Log('Set the following openai parameters: api_type: ' + str(openai.api_type) + ', api_version: ' + str(openai.api_version) + ', api_base: ' + str(openai.api_base) + ', api_key: XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX')

    parser_chat_session = ParserChatSession(temperature=temperature)  
    Print_And_Log('Created a new chat session for parser')    
    Print_And_Log('Sending user chat to OpenAI: ' + str(prompt) + '')
    parser_response_data = {}
    parser_response_data['chat_response'] = ""
    parser_response_data['function_response'] = ""
    parser_response_data['user_exit'] = False
    parser_chat_session.chat(prompt, parser_response_data, True)
    parser_function_response = parser_response_data['function_response']

    parser_chat_session = None #always clear this; we don't need conversation context for parser

    if(parser_function_response != ""):
        answerer_response_data['chat_response'] = f"Parser Output:{parser_response_data['function_response']}"

        #For later:

        # if answerer_chat_session is None:  
        #     answerer_chat_session = AnswererChatSession(temperature=temperature)  
        #     Print_And_Log('Created a new chat session for answerer')  
        # else:  
        #     Print_And_Log('Using existing chat session for answerer')  
        
        # answerer_chat_session.chat(parser_function_response, answerer_response_data, True)
    else:
        answerer_response_data['chat_response'] = "Couldn't parse any questions or topics"
    
    return

class ParserChatSession:
    history = [{"role": "system", "content": parser_system_message}]
    CurrentAISmartsLevel = AISmartsLevel.gpt4
    CurrentAIContextLength = GPT3Lengths.short
    
    def __init__(self, initial_system_message=parser_system_message, temperature=0.5): 

        self.temperature = temperature
        self.functions= [

            # |------ Parser Functions   ------|
            #TODO: turn these into lists
            #TODO: include confidence levels
            
            {
                "name": "parse_questions_and_topics",
                "description": "Return the questions and topics the conversation snippet might be about",
                "parameters": {
                    "type": "object",
                    "properties": {
                        "questions": {
                            "type": "array",  # Changed to array
                            "description": "questions relevant to the conversation, formatted with proper grammar ending in '?'",
                            "items": {"type": "string"}
                        },
                        "topics": {
                            "type": "array",  # Changed to array
                            "description": "topics the conversation is about, 3 word max",
                            "items": {"type": "string"}
                        }
                    }
                }
            },            
        ]
    
    def get_model_name(self):
        if self.CurrentAISmartsLevel == AISmartsLevel.gpt3:
            return GPT3_Models[self.CurrentAIContextLength]
        elif self.CurrentAISmartsLevel == AISmartsLevel.gpt4:
            return GPT4_Models[self.CurrentAIContextLength]
        else:
            raise ValueError("Invalid smarts level")

    def add_user_message(self, message):
        if self.history[-1]["role"] == "user":
            raise ValueError("Cannot add consecutive user messages. An assistant message should follow.")
        self.history.append({"role": "user", "content": message})
    
    def chat(self, message, parser_response_data, new_message = True): 
        if(new_message == True):
            self.add_user_message(message)
        Print_And_Log('Chatting OpenAI with engine: ' + self.get_model_name() + ' and new message: ' + str(message))
        max_retries = 2
        retry_wait_time = 12

        for attempt in range(max_retries):
            try:
                
                response = openai.ChatCompletion.create(
                    model=self.get_model_name(),
                    messages=self.history,
                    functions= self.functions,
                    function_call="auto",
                    temperature=self.temperature
                )
                Print_And_Log('OpenAI responded successfully')                
                self.parse_functions(response, parser_response_data)
                return
            except openai.error.RateLimitError as e:
                Print_And_Log(f"RateLimitError: {e}")
                time.sleep(retry_wait_time)
            except openai.error.InvalidRequestError as e:
                if "maximum context length" in str(e):
                    if (self.CurrentAISmartsLevel == AISmartsLevel.gpt3 and self.CurrentAIContextLength < GPT3Lengths.medium) or \
                        (self.CurrentAISmartsLevel == AISmartsLevel.gpt4 and self.CurrentAIContextLength < GPT4Lengths.medium):
                            self.CurrentAIContextLength = GPT3Lengths(self.CurrentAIContextLength.value + 1) if self.CurrentAISmartsLevel == AISmartsLevel.gpt3 else GPT4Lengths(self.CurrentAIContextLength.value + 1)
                            print("⚙️ Switched to:", GPT3_Models[self.CurrentAIContextLength] if self.CurrentAISmartsLevel == AISmartsLevel.gpt3 else GPT4_Models[self.CurrentAIContextLength])
                            # Continue with the new context length
                    else:
                        print("Max content length hit.  Trimming history so that it starts with the second-earliest user message")
                        self.trim_history()
                else:
                    print("Other InvalidRequestError occurred:", e)
                    # Handle other InvalidRequestErrors
        Print_And_Log('Too many rate limit errors.  Returning [AI model is too busy]')
        return "AI model is too busy"
                
    def parse_functions(self, openAI_response, parser_response_data):
        #print('Got a response from OpenAI: ' + str(openAI_response) + '')
        
        try:  #Sometimes responses are improperly formatted, so we catch that here and tell the LLM instead of failing
            finish_reason = openAI_response['choices'][0]['finish_reason']
            if finish_reason == "function_call":
                function_name = openAI_response['choices'][0]['message']['function_call']['name']
                arguments_str = openAI_response['choices'][0]['message']['function_call']['arguments']
                arguments_json = json.loads(arguments_str)
        except:
            generated_message = f"The function call was improperly formatted.  Please try again."  
            self.history.append({"role": "function","name": function_name, "content": generated_message})
            parser_response_data['function_response'] += generated_message

        if finish_reason == "function_call":
            if function_name == "parse_questions_and_topics":
                print(f"💭 The snippet seems to have questions or topics")
                #TODO: format questions and topics into parser_response_data['function_response']
                questions = arguments_json.get('questions', [])
                topics = arguments_json.get('topics', [])
                
                num_questions = len(questions)
                num_topics = len(topics)
                
                formatted_questions = f"{num_questions} questions generated: {', '.join(questions)}" if questions else 'No questions identified.'
                formatted_topics = f"{num_topics} topics identified: {', '.join(topics)}" if topics else 'No topics identified.'
        
                parser_response_data['function_response'] = f"{formatted_questions}\n{formatted_topics}"
            # if function_name == "dump_message_history":
            #     print(f"💭 The user asked me to export our current message history")
            #     filename = datetime.now().strftime("message_exports/Kevin_the_minion_message_history_%Y%m%d_%H%M%S.json")

            #     with open(filename, 'w') as file:
            #         json.dump(self.history, file, indent=4)

            #     print(f"File {filename} has been created and written to.")

            #     generated_message= 'I\'ve dumped message history to the file: ' + filename
            #     self.history.append({"role": "assistant", "content": generated_message})
            #     parser_response_data['chat_response'] = generated_message

        else:            
            generated_message = openAI_response['choices'][0]['message']['content']
            self.history.append({"role": "assistant", "content": generated_message})
            parser_response_data['chat_response'] = generated_message

        return

class AnswererChatSession:
    history = [{"role": "system", "content": answerer_system_message}]  # Moved history to a class variable 
    CurrentAISmartsLevel = AISmartsLevel.gpt3
    CurrentAIContextLength = GPT3Lengths.short
    
    def __init__(self, initial_system_message=answerer_system_message, temperature=0.5): 

        self.temperature = temperature
        self.functions= [
            # TODO: Add answerer functions
            # |------ Answerer Functions ------|
            
            {
            }
        ]

    def get_model_name(self):
        if self.CurrentAISmartsLevel == AISmartsLevel.gpt3:
            return GPT3_Models[self.CurrentAIContextLength]
        elif self.CurrentAISmartsLevel == AISmartsLevel.gpt4:
            return GPT4_Models[self.CurrentAIContextLength]
        else:
            raise ValueError("Invalid smarts level")

    def add_user_message(self, message):
        if self.history[-1]["role"] == "user":
            raise ValueError("Cannot add consecutive user messages. An assistant message should follow.")
        self.history.append({"role": "user", "content": message})

    def trim_history(self):
        trimmed_history = []
        user_count = 0

        # Always keep the first "system" role item
        if self.history and self.history[0]['role'] == 'system':
            trimmed_history.append(self.history[0])

        # Iterate through the rest of the history
        for message in self.history[1:]:
            if message['role'] == 'user':
                user_count += 1

            # If the second "user" message is encountered, keep it and all subsequent messages
            if user_count >= 2:
                trimmed_history.append(message)

        self.history = trimmed_history

    def clear_history(self):
        self.history = [{"role": "system", "content": self.history[0]['content']}]

    def chat(self, message, answerer_response_data, new_message = True): 
        if(new_message == True):
            self.add_user_message(message)
        Print_And_Log('Chatting OpenAI with engine: ' + self.get_model_name() + ' and new message: ' + str(message))
        max_retries = 8
        retry_wait_time = 12

        for attempt in range(max_retries):
            try:
                response = openai.ChatCompletion.create(
                    model=self.get_model_name(),
                    messages=self.history,
                    functions= self.functions,
                    function_call="auto",
                    temperature=self.temperature
                )
                Print_And_Log('OpenAI responded successfully')                
                self.parse_functions(response, answerer_response_data)
                return
            except openai.error.RateLimitError as e:
                Print_And_Log(f"RateLimitError: {e}")
                time.sleep(retry_wait_time)
            except openai.error.InvalidRequestError as e:
                if "maximum context length" in str(e):
                    if (self.CurrentAISmartsLevel == AISmartsLevel.gpt3 and self.CurrentAIContextLength < GPT3Lengths.medium) or \
                        (self.CurrentAISmartsLevel == AISmartsLevel.gpt4 and self.CurrentAIContextLength < GPT4Lengths.medium):
                            self.CurrentAIContextLength = GPT3Lengths(self.CurrentAIContextLength.value + 1) if self.CurrentAISmartsLevel == AISmartsLevel.gpt3 else GPT4Lengths(self.CurrentAIContextLength.value + 1)
                            print("⚙️ Switched to:", GPT3_Models[self.CurrentAIContextLength] if self.CurrentAISmartsLevel == AISmartsLevel.gpt3 else GPT4_Models[self.CurrentAIContextLength])
                            # Continue with the new context length
                    else:
                        print("Max content length hit.  Trimming history so that it starts with the second-earliest user message")
                        self.trim_history()
                else:
                    print("Other InvalidRequestError occurred:", e)
                    # Handle other InvalidRequestErrors
        Print_And_Log('Too many rate limit errors.  Returning [AI model is too busy]')
        return "AI model is too busy"
                
    def parse_functions(self, openAI_response, response_data):
        #print('Got a response from OpenAI: ' + str(openAI_response) + '')
        
        try:  #Sometimes responses are improperly formatted, so we catch that here and tell the LLM instead of failing
            finish_reason = openAI_response['choices'][0]['finish_reason']
            if finish_reason == "function_call":
                function_name = openAI_response['choices'][0]['message']['function_call']['name']
                arguments_str = openAI_response['choices'][0]['message']['function_call']['arguments']
                arguments_json = json.loads(arguments_str)
        except:
            generated_message = f"The function call was improperly formatted.  Please try again."  
            self.history.append({"role": "function","name": function_name, "content": generated_message})
            response_data['function_response'] += generated_message

        if finish_reason == "function_call":
            #Todo: Deal with answerer functions
            pass

        else:            
            generated_message = openAI_response['choices'][0]['message']['content']
            self.history.append({"role": "assistant", "content": generated_message})
            response_data['chat_response'] = generated_message

        return