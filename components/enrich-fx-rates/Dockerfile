FROM python:3.12-slim
WORKDIR /component
COPY main.py manifest.json ./
# The input path arrives as an environment variable. The result is printed to stdout for the platform to store.
ENTRYPOINT ["python", "main.py"]
