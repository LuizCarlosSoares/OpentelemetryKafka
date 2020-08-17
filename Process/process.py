from flask import render_template
from waitress import serve
import connexion
import os

app = connexion.App(__name__,specification_dir="./config")
app.add_api('swagger.yml')

@app.rout('/')
def home():
    return render_template('home.html')

if __name__ == '__main__':
    if os.getenv('PYTHON_ENV') == 'production':
        serve(app,host='0.0.0.0',port=5000)
    else:
        app.run(host='0.0.0.0',port=5000,debug=True)