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
| GetOrCreateExistingKey | 12          |   170.6 ns |  35.11 ns |  1.92 ns |  1.27 |    0.01 |    3 | 0.0196 |      - |     328 B |        0.93 |
| IsKeyDestroyed         | 12          |   111.2 ns |  14.19 ns |  0.78 ns |  0.82 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.32 |
| GetActiveKeysCount     | 12          | 2,709.7 ns | 111.57 ns |  6.12 ns | 20.10 |    0.12 |    4 | 0.1450 |      - |    2464 B |        7.00 |
| CreateNewKey           | 12          | 4,094.5 ns | 945.02 ns | 51.80 ns | 30.37 |    0.38 |    5 | 0.0496 | 0.0458 |     848 B |        2.41 |
| GetExistingKey         | 12          |   134.8 ns |  16.48 ns |  0.90 ns |  1.00 |    0.01 |    2 | 0.0210 |      - |     352 B |        1.00 |
