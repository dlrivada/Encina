```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 105.233 ns | 10.209 ns | 0.5596 ns | 16.98 |    0.58 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   6.204 ns |  4.369 ns | 0.2395 ns |  1.00 |    0.05 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 449.000 ns | 78.596 ns | 4.3081 ns | 72.45 |    2.53 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 433.089 ns | 69.210 ns | 3.7936 ns | 69.88 |    2.43 | 0.0601 |    1008 B |       42.00 |
