```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.51GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | PeriodCount | Mean        | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------ |------------:|------------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| GetOrCreateExistingKey | 12          |    90.56 ns |    16.23 ns |  0.890 ns |  1.20 |    0.02 |    3 | 0.0196 |      - |     328 B |        0.93 |
| IsKeyDestroyed         | 12          |    57.94 ns |    16.78 ns |  0.920 ns |  0.77 |    0.02 |    1 | 0.0067 |      - |     112 B |        0.32 |
| GetActiveKeysCount     | 12          | 1,548.88 ns |   824.48 ns | 45.192 ns | 20.57 |    0.61 |    4 | 0.1469 |      - |    2464 B |        7.00 |
| CreateNewKey           | 12          | 2,870.63 ns | 1,659.22 ns | 90.948 ns | 38.13 |    1.21 |    5 | 0.0496 | 0.0458 |     848 B |        2.41 |
| GetExistingKey         | 12          |    75.30 ns |    25.19 ns |  1.381 ns |  1.00 |    0.02 |    2 | 0.0210 |      - |     352 B |        1.00 |
