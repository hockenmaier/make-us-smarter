import os
import openai

openai.api_key = os.environ["openai_api_key"]

def main():
    response = openai.Completion.create(
        model="text-davinci-003",
        prompt="this is a test prompt, please respond that you got it",
        temperature=0.6,
    )
    return response

def chat():
    history = [{"role": "system", "content": "You are a helpful assistant"}]
    history.append({"role": "user", "content": "this is a test prompt, please respond that you got it"})
    response = openai.ChatCompletion.create(
        model="gpt-3.5-turbo",
        messages=history
    )

    return response

if __name__ == "__main__":
    for(i) in range(0, 10):
         print("")
    print("Test Generic OpenAI Call")
    #print(main())
    print(chat())