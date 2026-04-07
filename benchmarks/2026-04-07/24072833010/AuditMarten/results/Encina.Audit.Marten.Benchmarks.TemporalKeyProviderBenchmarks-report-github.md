```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  Dry    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

```
| Method                 | PeriodCount | Mean      | Error | Ratio | Rank | Allocated | Alloc Ratio |
|----------------------- |------------ |----------:|------:|------:|-----:|----------:|------------:|
| GetOrCreateExistingKey | 12          |  6.139 ms |    NA |  0.42 |    1 |     328 B |        0.93 |
| IsKeyDestroyed         | 12          | 10.639 ms |    NA |  0.72 |    3 |     112 B |        0.32 |
| GetActiveKeysCount     | 12          | 11.560 ms |    NA |  0.78 |    4 |    2464 B |        7.00 |
| CreateNewKey           | 12          |  7.784 ms |    NA |  0.53 |    2 |     848 B |        2.41 |
| GetExistingKey         | 12          | 14.736 ms |    NA |  1.00 |    5 |     352 B |        1.00 |
