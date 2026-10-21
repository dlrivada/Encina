```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.62GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error      | StdDev     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|-----------:|-----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 132.377 ns |   7.804 ns |  0.4278 ns | 16.22 |    0.33 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   8.164 ns |   3.492 ns |  0.1914 ns |  1.00 |    0.03 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 636.249 ns | 244.874 ns | 13.4224 ns | 77.96 |    2.11 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 603.402 ns |  45.499 ns |  2.4939 ns | 73.93 |    1.50 | 0.0601 |    1008 B |       42.00 |
