```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

```
| Method                                  | Mean     | Error | Ratio | Allocated | Alloc Ratio |
|---------------------------------------- |---------:|------:|------:|----------:|------------:|
| Stream_SmallDataset_10Items             | 66.80 ms |    NA |  1.00 |   17320 B |        1.00 |
| Stream_MediumDataset_100Items           | 66.70 ms |    NA |  1.00 |   36456 B |        2.10 |
| Stream_LargeDataset_1000Items           | 66.60 ms |    NA |  1.00 |  230392 B |       13.30 |
| Stream_WithPipelineBehaviors            | 78.54 ms |    NA |  1.18 |   36240 B |        2.09 |
| Stream_MaterializeToList_100Items       | 67.58 ms |    NA |  1.01 |   45064 B |        2.60 |
| Stream_CountOnly_NoMaterialization      | 66.82 ms |    NA |  1.00 |   35248 B |        2.04 |
| Stream_WithCancellation_EarlyExit       | 68.74 ms |    NA |  1.03 |   53200 B |        3.07 |
| Stream_DirectHandlerInvocation_NoEncina | 14.39 ms |    NA |  0.22 |     400 B |        0.02 |
