```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.72GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error      | StdDev     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|-----------:|-----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 134.683 ns |   9.499 ns |  0.5207 ns | 15.72 |    0.46 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   8.575 ns |   5.192 ns |  0.2846 ns |  1.00 |    0.04 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 629.358 ns | 317.080 ns | 17.3802 ns | 73.45 |    2.76 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 558.683 ns | 128.476 ns |  7.0422 ns | 65.20 |    2.01 | 0.0601 |    1008 B |       42.00 |
