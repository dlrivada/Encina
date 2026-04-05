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
| Stream_SmallDataset_10Items             | 67.90 ms |    NA |  1.00 |   17248 B |        1.00 |
| Stream_MediumDataset_100Items           | 66.89 ms |    NA |  0.99 |   35288 B |        2.05 |
| Stream_LargeDataset_1000Items           | 67.63 ms |    NA |  1.00 |  230392 B |       13.36 |
| Stream_WithPipelineBehaviors            | 79.83 ms |    NA |  1.18 |   43688 B |        2.53 |
| Stream_MaterializeToList_100Items       | 68.27 ms |    NA |  1.01 |   51176 B |        2.97 |
| Stream_CountOnly_NoMaterialization      | 66.99 ms |    NA |  0.99 |   37064 B |        2.15 |
| Stream_WithCancellation_EarlyExit       | 70.04 ms |    NA |  1.03 |   52720 B |        3.06 |
| Stream_DirectHandlerInvocation_NoEncina | 14.63 ms |    NA |  0.22 |     400 B |        0.02 |
