import sys
import requests

url = "https://yamato-api.prd.cds.internal.unity3d.com/jobs"

branch_name = sys.argv[1]
git_revision = sys.argv[2]
key = 'ApiKey ' + sys.argv[3]

data = '''{
  "source": {
    "branchname": "'''+ branch_name +'''",
    "revision": "''' + git_revision + '''"
  },
  "links": {
    "project": "/projects/78",
    "jobDefinition": "/projects/78/revisions/''' + git_revision + '''/job-definitions/.yamato#upm-ci-abv.yml#trunk_verification"
  }
}'''


response = requests.post(url, data=data, headers={'Authorization': key})

response_json = response.json()
job_id = response_json['id']
get_job = requests.get(url + '/' + job_id, headers={'Authorization': key})
job_json = get_job.json()
status = job_json['status']

if status == 'success':
  print('Success: Check job at https://yamato.prd.cds.internal.unity3d.com/job/'+job_id)
  sys.exit(0)
else:
  print('Failed to start job')
  sys.exit(1)