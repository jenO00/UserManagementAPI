# UserManagementAPI
Part of coursera Microsoft course. The goal is to create a User management API in .NET and C#
A USER model is used in the user management API.

## CRUD
The following CRUD endpoints were implemented: 
* CREATE - POST localhost:XXXX/users
* READ - GET localhost:XXXX/users || GET localhost:XXXX/users/{id}
* UPDATE - PUT localhost:XXXX/users 
* DELETE - DELTE localhost:XXXX/users/{id}


## Validation functionality
Validation functionality is implemented, which is utilized for the POST and PUT functions. 
Checks if the user is valid.

## Simulated API KEY
During this project, a simulated API Key is used for educational purposes. I am not comfortable sharing a real API key. 
In postman, the key is used the following way: 
<img width="1276" height="217" alt="image" src="https://github.com/user-attachments/assets/c7f91387-9b4d-4a94-9a68-8cf8eb5615b0" />

In real deployment, a safer method should be considered, such as: 
OAUTH, JWT. 

# Example Usages
Example usages are shown below.

## CREATE (POST)
Example of the POST usage. 
<img width="1276" height="669" alt="image" src="https://github.com/user-attachments/assets/76d24171-42f4-4b6f-976a-59c5b7c0d762" />
  
## READ (GET)
Example of GET usage
<img width="1239" height="756" alt="image" src="https://github.com/user-attachments/assets/693aa5e4-feb2-43ee-a5e0-52c41ce6aae7" />
One can also get specific users by utilizing e.g. GET http://localhost:XXXX/users/2, where 2 is the user id. 

## UPDATE (PUT)
Example of update usage.
Before: <img width="399" height="167" alt="image" src="https:/ /github.com/user-attachments/assets/4fc7c16c-eaef-407b-b5fc-f7e950f30564" />
After: <img width="1198" height="275" alt="image" src="https://github.com/user-attachments/assets/a3c0df21-0a57-40b6-bd81-b7bf88e73ea9" />


## DELETE (DELETE) 
Example of delete usage, where Lisa, id 4, is deleted.
GET users before Lisa is deleted:
```
[
    {
        "id": 1,
        "name": "Bengt",
        "department": "HR",
        "email": "bengt@gmail.se"
    },
    {
        "id": 3,
        "name": "Alice",
        "department": "IT",
        "email": "alice@gmail.se"
    },
    {
        "id": 4,
        "name": "Lisa",
        "department": "Security",
        "email": "lisa@gmail.se"
    }
]
```
Deleting Lisa:
<img width="1002" height="409" alt="image" src="https://github.com/user-attachments/assets/98c72450-80db-4318-b499-3430b8d485a4" />

Getting the users after this will give the following list:
```
[
    {
        "id": 1,
        "name": "Bengt",
        "department": "HR",
        "email": "bengt@gmail.se"
    },
    {
        "id": 3,
        "name": "Alice",
        "department": "IT",
        "email": "alice@gmail.se"
    }
]
```


