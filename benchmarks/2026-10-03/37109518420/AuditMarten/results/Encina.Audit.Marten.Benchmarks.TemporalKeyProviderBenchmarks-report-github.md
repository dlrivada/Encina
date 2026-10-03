```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 3.19GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | PeriodCount | Mean       | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------ |-----------:|----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| GetOrCreateExistingKey | 12          |   163.5 ns |  13.39 ns |  0.73 ns |  1.20 |    0.01 |    2 | 0.0196 |      - |     328 B |        0.93 |
| IsKeyDestroyed         | 12          |   110.2 ns |   5.24 ns |  0.29 ns |  0.81 |    0.00 |    1 | 0.0067 |      - |     112 B |        0.32 |
| GetActiveKeysCount     | 12          | 2,815.1 ns | 313.50 ns | 17.18 ns | 20.66 |    0.11 |    3 | 0.1450 |      - |    2464 B |        7.00 |
| CreateNewKey           | 12          | 4,038.6 ns | 403.49 ns | 22.12 ns | 29.63 |    0.15 |    4 | 0.0458 | 0.0381 |     848 B |        2.41 |
| GetExistingKey         | 12          |   136.3 ns |   4.79 ns |  0.26 ns |  1.00 |    0.00 |    2 | 0.0210 |      - |     352 B |        1.00 |
