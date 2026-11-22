```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.64GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method           | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 139.266 ns | 0.4917 ns | 0.7359 ns | 16.13 |    0.24 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   8.634 ns | 0.0812 ns | 0.1216 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 639.211 ns | 3.2286 ns | 4.7324 ns | 74.05 |    1.15 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 580.823 ns | 3.0025 ns | 4.3061 ns | 67.28 |    1.05 | 0.0601 |    1008 B |       42.00 |
