```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | PeriodCount | Mean       | Error     | StdDev  | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------ |-----------:|----------:|--------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| GetOrCreateExistingKey | 12          |   166.1 ns |  14.46 ns | 0.79 ns |  1.16 |    0.01 |    1 | 0.0196 |      - |     328 B |        0.93 |
| IsKeyDestroyed         | 12          |   120.8 ns |   5.48 ns | 0.30 ns |  0.84 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.32 |
| GetActiveKeysCount     | 12          | 2,815.7 ns | 177.62 ns | 9.74 ns | 19.66 |    0.16 |    2 | 0.1450 |      - |    2464 B |        7.00 |
| CreateNewKey           | 12          | 3,964.4 ns |  26.53 ns | 1.45 ns | 27.67 |    0.21 |    3 | 0.0458 | 0.0381 |     848 B |        2.41 |
| GetExistingKey         | 12          |   143.3 ns |  22.54 ns | 1.24 ns |  1.00 |    0.01 |    1 | 0.0210 |      - |     352 B |        1.00 |
