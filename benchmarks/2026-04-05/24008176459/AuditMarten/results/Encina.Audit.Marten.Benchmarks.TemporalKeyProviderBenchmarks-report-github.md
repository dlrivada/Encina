```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

```
| Method                 | PeriodCount | Mean      | Error | Ratio | Rank | Allocated | Alloc Ratio |
|----------------------- |------------ |----------:|------:|------:|-----:|----------:|------------:|
| **GetExistingKey**         | **12**          | **15.048 ms** |    **NA** |  **1.00** |    **5** |     **352 B** |        **1.00** |
| GetOrCreateExistingKey | 12          |  6.204 ms |    NA |  0.41 |    1 |     328 B |        0.93 |
| CreateNewKey           | 12          |  7.490 ms |    NA |  0.50 |    2 |     848 B |        2.41 |
| IsKeyDestroyed         | 12          | 10.191 ms |    NA |  0.68 |    3 |     112 B |        0.32 |
| GetActiveKeysCount     | 12          | 11.526 ms |    NA |  0.77 |    4 |    2464 B |        7.00 |
|                        |             |           |       |       |      |           |             |
| **GetExistingKey**         | **84**          | **15.076 ms** |    **NA** |  **1.00** |    **5** |     **352 B** |        **1.00** |
| GetOrCreateExistingKey | 84          |  6.173 ms |    NA |  0.41 |    1 |     328 B |        0.93 |
| CreateNewKey           | 84          |  7.548 ms |    NA |  0.50 |    2 |     848 B |        2.41 |
| IsKeyDestroyed         | 84          | 10.210 ms |    NA |  0.68 |    3 |     112 B |        0.32 |
| GetActiveKeysCount     | 84          | 11.247 ms |    NA |  0.75 |    4 |   15272 B |       43.39 |
