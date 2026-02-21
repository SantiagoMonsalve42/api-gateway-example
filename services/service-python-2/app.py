from fastapi import FastAPI, Request
import os

app = FastAPI()

# Identificador de instancia
INSTANCE_NAME = os.getenv("INSTANCE_NAME", "Instance-2")

@app.get("/files")
def get_files(request: Request):
    return {
        "instance": INSTANCE_NAME,
        "files": [
            { "id": "FILE-001", "path": "/files/001", "name": "file_0001.txt" },
            { "id": "FILE-002", "path": "/files/002", "name": "file_0002.txt" },
            { "id": "FILE-003", "path": "/files/003", "name": "file_0003.txt" }
        ],
    }
