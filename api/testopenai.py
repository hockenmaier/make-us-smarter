import os
import openai
import requests
import json

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

def testEndpoint():

    url = 'https://kbr1ahdla6.execute-api.us-west-2.amazonaws.com/snippet'
    payload = {
        "payload": {
            "text_snippet": "the area under a curve.  Is called what.  I don't know what it",
            "use_test_data": False
        }
    }
    headers = {'Content-Type': 'application/json'}

    response = requests.post(url, data=json.dumps(payload), headers=headers)
    print(response.json())
    response_json = response.json()  # Parse JSON response
    user_exit_value = response_json.get('user_exit', None)  # Extract 'user_exit' value
    # Get the response text or JSON
    print(response.status_code)
    print(f"User Exit: {response_json.get('user_exit', None)}")
    print(f"Message: {response_json.get('message', None)}")
    print(f"Question Answer Pairs: {response_json.get('qna_pairs', None)}")
    return


if __name__ == "__main__":
    for(i) in range(0, 4):
         print("")
    print("Test Generic OpenAI Call")
    #print(main())
    #print(chat())
    testEndpoint()