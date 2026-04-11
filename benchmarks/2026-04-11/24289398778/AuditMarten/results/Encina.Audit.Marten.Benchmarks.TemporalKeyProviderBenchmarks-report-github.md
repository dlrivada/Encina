```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                 | PeriodCount | Mean       | Error    | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------ |-----------:|---------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| GetOrCreateExistingKey | 12          |   158.1 ns |  0.65 ns |   0.89 ns |  1.13 |    0.03 |    3 | 0.0196 |      - |     328 B |        0.93 |
| IsKeyDestroyed         | 12          |   111.3 ns |  0.93 ns |   1.39 ns |  0.80 |    0.02 |    1 | 0.0067 |      - |     112 B |        0.32 |
| GetActiveKeysCount     | 12          | 2,774.5 ns |  8.64 ns |  12.11 ns | 19.83 |    0.44 |    4 | 0.1450 |      - |    2464 B |        7.00 |
| CreateNewKey           | 12          | 4,239.1 ns | 83.65 ns | 119.97 ns | 30.30 |    1.07 |    5 | 0.0496 | 0.0458 |     848 B |        2.41 |
| GetExistingKey         | 12          |   140.0 ns |  2.16 ns |   3.16 ns |  1.00 |    0.03 |    2 | 0.0210 |      - |     352 B |        1.00 |
