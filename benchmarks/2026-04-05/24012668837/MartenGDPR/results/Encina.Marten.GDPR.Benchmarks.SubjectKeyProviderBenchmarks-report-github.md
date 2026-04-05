```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

```
| Method                 | SubjectCount | Mean      | Error | Ratio | Rank | Allocated | Alloc Ratio |
|----------------------- |------------- |----------:|------:|------:|-----:|----------:|------------:|
| **GetExistingKey**         | **10**           | **15.406 ms** |    **NA** |  **1.00** |    **4** |     **272 B** |        **1.00** |
| GetOrCreateExistingKey | 10           |  6.237 ms |    NA |  0.40 |    1 |     248 B |        0.91 |
| CreateNewKey           | 10           |  7.484 ms |    NA |  0.49 |    2 |     928 B |        3.41 |
| CheckIsForgotten       | 10           | 10.153 ms |    NA |  0.66 |    3 |     112 B |        0.41 |
|                        |              |           |       |       |      |           |             |
| **GetExistingKey**         | **100**          | **15.128 ms** |    **NA** |  **1.00** |    **4** |     **272 B** |        **1.00** |
| GetOrCreateExistingKey | 100          |  6.650 ms |    NA |  0.44 |    1 |     248 B |        0.91 |
| CreateNewKey           | 100          |  7.455 ms |    NA |  0.49 |    2 |     928 B |        3.41 |
| CheckIsForgotten       | 100          | 10.201 ms |    NA |  0.67 |    3 |     112 B |        0.41 |
