```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | PeriodCount | Mean        | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------ |------------:|-----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| GetOrCreateExistingKey | 12          |    95.04 ns |  47.337 ns |  2.595 ns |  1.23 |    0.03 |    3 | 0.0196 |      - |     328 B |        0.93 |
| IsKeyDestroyed         | 12          |    62.15 ns |  46.021 ns |  2.523 ns |  0.80 |    0.03 |    1 | 0.0067 |      - |     112 B |        0.32 |
| GetActiveKeysCount     | 12          | 1,613.74 ns | 184.418 ns | 10.109 ns | 20.87 |    0.15 |    4 | 0.1469 |      - |    2464 B |        7.00 |
| CreateNewKey           | 12          | 3,064.01 ns | 737.175 ns | 40.407 ns | 39.62 |    0.49 |    5 | 0.0496 | 0.0458 |     848 B |        2.41 |
| GetExistingKey         | 12          |    77.34 ns |   7.280 ns |  0.399 ns |  1.00 |    0.01 |    2 | 0.0210 |      - |     352 B |        1.00 |
