from django.shortcuts import render_to_response
from django.template import RequestContext

def design(request):
  return render_to_response('design.html', {
                              'something' : 'yaya data',
                              }, context_instance = RequestContext(request))
