```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | PeriodCount | Mean       | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------ |-----------:|----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| GetOrCreateExistingKey | 12          |   170.5 ns |  27.58 ns |  1.51 ns |  1.21 |    0.03 |    3 | 0.0196 |      - |     328 B |        0.93 |
| IsKeyDestroyed         | 12          |   109.7 ns |  22.03 ns |  1.21 ns |  0.78 |    0.02 |    1 | 0.0067 |      - |     112 B |        0.32 |
| GetActiveKeysCount     | 12          | 2,717.6 ns |  70.68 ns |  3.87 ns | 19.24 |    0.39 |    4 | 0.1450 |      - |    2464 B |        7.00 |
| CreateNewKey           | 12          | 3,976.7 ns | 397.33 ns | 21.78 ns | 28.15 |    0.58 |    5 | 0.0458 | 0.0381 |     848 B |        2.41 |
| GetExistingKey         | 12          |   141.3 ns |  60.55 ns |  3.32 ns |  1.00 |    0.03 |    2 | 0.0210 |      - |     352 B |        1.00 |
