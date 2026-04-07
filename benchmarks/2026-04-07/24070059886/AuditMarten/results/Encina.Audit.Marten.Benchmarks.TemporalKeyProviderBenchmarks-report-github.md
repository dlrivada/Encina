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
| GetOrCreateExistingKey | 12          |  6.379 ms |    NA |  0.40 |    1 |     328 B |        0.93 |
| IsKeyDestroyed         | 12          | 10.381 ms |    NA |  0.66 |    3 |     112 B |        0.32 |
| GetActiveKeysCount     | 12          | 11.721 ms |    NA |  0.74 |    4 |    2464 B |        7.00 |
| CreateNewKey           | 12          |  8.305 ms |    NA |  0.53 |    2 |     848 B |        2.41 |
| GetExistingKey         | 12          | 15.804 ms |    NA |  1.00 |    5 |     352 B |        1.00 |
