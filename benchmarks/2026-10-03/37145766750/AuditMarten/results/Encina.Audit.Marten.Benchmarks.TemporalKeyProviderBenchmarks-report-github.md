```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | PeriodCount | Mean       | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------ |-----------:|----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| GetOrCreateExistingKey | 12          |   172.9 ns |  19.68 ns |  1.08 ns |  1.19 |    0.03 |    2 | 0.0196 |      - |     328 B |        0.93 |
| IsKeyDestroyed         | 12          |   107.9 ns |  16.28 ns |  0.89 ns |  0.74 |    0.02 |    1 | 0.0067 |      - |     112 B |        0.32 |
| GetActiveKeysCount     | 12          | 2,851.0 ns | 303.99 ns | 16.66 ns | 19.57 |    0.46 |    3 | 0.1450 |      - |    2464 B |        7.00 |
| CreateNewKey           | 12          | 4,265.1 ns | 456.23 ns | 25.01 ns | 29.27 |    0.68 |    4 | 0.0458 | 0.0381 |     848 B |        2.41 |
| GetExistingKey         | 12          |   145.8 ns |  70.76 ns |  3.88 ns |  1.00 |    0.03 |    2 | 0.0210 |      - |     352 B |        1.00 |
