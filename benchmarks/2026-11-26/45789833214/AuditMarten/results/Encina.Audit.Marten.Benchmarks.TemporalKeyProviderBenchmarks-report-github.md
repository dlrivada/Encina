```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.13GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | PeriodCount | Mean       | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------ |-----------:|----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| GetOrCreateExistingKey | 12          |   166.3 ns |  16.73 ns |  0.92 ns |  1.20 |    0.01 |    2 | 0.0196 |      - |     328 B |        0.93 |
| IsKeyDestroyed         | 12          |   111.1 ns |   7.95 ns |  0.44 ns |  0.80 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.32 |
| GetActiveKeysCount     | 12          | 2,721.9 ns | 260.79 ns | 14.29 ns | 19.63 |    0.16 |    3 | 0.1450 |      - |    2464 B |        7.00 |
| CreateNewKey           | 12          | 3,948.3 ns | 492.94 ns | 27.02 ns | 28.48 |    0.25 |    4 | 0.0496 | 0.0458 |     848 B |        2.41 |
| GetExistingKey         | 12          |   138.6 ns |  19.04 ns |  1.04 ns |  1.00 |    0.01 |    2 | 0.0210 |      - |     352 B |        1.00 |
