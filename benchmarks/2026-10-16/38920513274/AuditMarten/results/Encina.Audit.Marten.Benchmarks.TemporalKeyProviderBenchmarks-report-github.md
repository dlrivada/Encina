```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | PeriodCount | Mean        | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------ |------------:|-----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| GetOrCreateExistingKey | 12          |   129.15 ns |  31.721 ns |  1.739 ns |  1.21 |    0.01 |    3 | 0.0196 |      - |     328 B |        0.93 |
| IsKeyDestroyed         | 12          |    83.50 ns |  12.962 ns |  0.711 ns |  0.79 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.32 |
| GetActiveKeysCount     | 12          | 2,109.46 ns | 224.493 ns | 12.305 ns | 19.84 |    0.12 |    4 | 0.1450 |      - |    2464 B |        7.00 |
| CreateNewKey           | 12          | 3,516.49 ns | 407.027 ns | 22.311 ns | 33.08 |    0.21 |    5 | 0.0496 | 0.0458 |     848 B |        2.41 |
| GetExistingKey         | 12          |   106.32 ns |   7.704 ns |  0.422 ns |  1.00 |    0.00 |    2 | 0.0210 |      - |     352 B |        1.00 |
