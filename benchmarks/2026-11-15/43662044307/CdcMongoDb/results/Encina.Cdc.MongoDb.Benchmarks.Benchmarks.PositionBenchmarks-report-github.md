```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method           | Mean       | Error     | StdDev     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|----------:|-----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 107.703 ns | 1.4524 ns |  2.1738 ns | 15.20 |    0.58 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   7.094 ns | 0.1621 ns |  0.2324 ns |  1.00 |    0.05 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 485.233 ns | 6.8421 ns | 10.2409 ns | 68.47 |    2.66 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 474.926 ns | 2.3342 ns |  3.4215 ns | 67.02 |    2.25 | 0.0601 |    1008 B |       42.00 |
