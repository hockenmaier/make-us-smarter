from __init__ import Main_Loop as function_main

#Test code for user interaction via terminal

def interact(multiline:bool = False):
    while True:
        # Accept user input
        if multiline:
            lines = []
            print("Enter your multi-line text and add a line with the word 'END' to finish:")
            while True:
                line = input()
                if line == 'END':
                    break
                lines.append(line)

            user_input = '\n'.join(lines)
        else:
            user_input = input(">>> ")
        # Special Operations
        if user_input.lower() == 'exit':
            break
        
        if user_input.lower() == 'm':
            multiline = not multiline
            continue
        
        # Send messages to main function
        payload = {
            "text_snippet": user_input
        }

        response = function_main(payload)

        if response['user_exit'] == True:
            print(f"Ending Conversation!")
            break
        
        print(response['message'])
        

    print("---end of test---")

if __name__ == "__main__":
    for(i) in range(0, 10):
         print("")
    print("Test Terminal with Parser/Answerer Flow")
    interact()