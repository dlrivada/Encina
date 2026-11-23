```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 105.192 ns | 19.663 ns | 1.0778 ns | 15.80 |    0.18 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   6.658 ns |  1.018 ns | 0.0558 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 437.802 ns | 24.927 ns | 1.3663 ns | 65.76 |    0.51 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 433.052 ns | 52.293 ns | 2.8663 ns | 65.05 |    0.60 | 0.0601 |    1008 B |       42.00 |
