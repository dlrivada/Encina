```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 3.02GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | PeriodCount | Mean       | Error       | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------ |-----------:|------------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| GetOrCreateExistingKey | 12          |   165.9 ns |    11.19 ns |  0.61 ns |  1.17 |    0.01 |    2 | 0.0196 |      - |     328 B |        0.93 |
| IsKeyDestroyed         | 12          |   112.4 ns |     5.82 ns |  0.32 ns |  0.79 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.32 |
| GetActiveKeysCount     | 12          | 2,814.4 ns |    68.72 ns |  3.77 ns | 19.77 |    0.19 |    3 | 0.1450 |      - |    2464 B |        7.00 |
| CreateNewKey           | 12          | 4,027.9 ns | 1,041.72 ns | 57.10 ns | 28.29 |    0.44 |    4 | 0.0458 | 0.0381 |     848 B |        2.41 |
| GetExistingKey         | 12          |   142.4 ns |    28.86 ns |  1.58 ns |  1.00 |    0.01 |    2 | 0.0210 |      - |     352 B |        1.00 |
