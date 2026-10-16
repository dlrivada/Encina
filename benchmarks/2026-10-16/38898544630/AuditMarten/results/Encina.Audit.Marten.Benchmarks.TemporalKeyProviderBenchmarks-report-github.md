```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.85GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | PeriodCount | Mean       | Error       | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------ |-----------:|------------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| GetOrCreateExistingKey | 12          |   165.9 ns |     7.77 ns |  0.43 ns |  1.22 |    0.01 |    3 | 0.0196 |      - |     328 B |        0.93 |
| IsKeyDestroyed         | 12          |   110.3 ns |     9.27 ns |  0.51 ns |  0.81 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.32 |
| GetActiveKeysCount     | 12          | 3,102.0 ns |    97.52 ns |  5.35 ns | 22.76 |    0.26 |    4 | 0.1450 |      - |    2464 B |        7.00 |
| CreateNewKey           | 12          | 4,129.3 ns | 1,340.84 ns | 73.50 ns | 30.30 |    0.58 |    5 | 0.0496 | 0.0458 |     848 B |        2.41 |
| GetExistingKey         | 12          |   136.3 ns |    32.25 ns |  1.77 ns |  1.00 |    0.02 |    2 | 0.0210 |      - |     352 B |        1.00 |
