```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | PeriodCount | Mean        | Error       | StdDev     | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------ |------------:|------------:|-----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| GetOrCreateExistingKey | 12          |   144.37 ns |    23.91 ns |   1.311 ns |  0.96 |    0.03 |    2 | 0.0038 |      - |     328 B |        0.93 |
| IsKeyDestroyed         | 12          |    89.03 ns |    56.87 ns |   3.117 ns |  0.59 |    0.03 |    1 | 0.0013 |      - |     112 B |        0.32 |
| GetActiveKeysCount     | 12          | 2,692.20 ns |   435.16 ns |  23.853 ns | 17.94 |    0.57 |    3 | 0.0267 |      - |    2464 B |        7.00 |
| CreateNewKey           | 12          | 3,320.25 ns | 2,588.08 ns | 141.861 ns | 22.13 |    1.06 |    4 | 0.0076 | 0.0038 |     848 B |        2.41 |
| GetExistingKey         | 12          |   150.16 ns |    97.73 ns |   5.357 ns |  1.00 |    0.04 |    2 | 0.0041 |      - |     352 B |        1.00 |
