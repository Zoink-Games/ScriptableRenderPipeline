import sys
import requests

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

if status == 'success':
  print('Success: Check job at https://yamato.prd.cds.internal.unity3d.com/job/'+job_id)
  sys.exit(0)
else:
  print('Failed to start job')
  sys.exit(1)