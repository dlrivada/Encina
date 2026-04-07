```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  Dry    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

```
| Method                 | SubjectCount | Mean      | Error | Ratio | Rank | Allocated | Alloc Ratio |
|----------------------- |------------- |----------:|------:|------:|-----:|----------:|------------:|
| **GetExistingKey**         | **10**           | **14.452 ms** |    **NA** |  **1.00** |    **4** |     **272 B** |        **1.00** |
| GetOrCreateExistingKey | 10           |  5.886 ms |    NA |  0.41 |    1 |     248 B |        0.91 |
| CreateNewKey           | 10           |  7.313 ms |    NA |  0.51 |    2 |     928 B |        3.41 |
| CheckIsForgotten       | 10           |  9.698 ms |    NA |  0.67 |    3 |     112 B |        0.41 |
|                        |              |           |       |       |      |           |             |
| **GetExistingKey**         | **100**          | **14.639 ms** |    **NA** |  **1.00** |    **4** |     **272 B** |        **1.00** |
| GetOrCreateExistingKey | 100          |  6.022 ms |    NA |  0.41 |    1 |     248 B |        0.91 |
| CreateNewKey           | 100          |  7.377 ms |    NA |  0.50 |    2 |     928 B |        3.41 |
| CheckIsForgotten       | 100          |  9.727 ms |    NA |  0.66 |    3 |     112 B |        0.41 |
