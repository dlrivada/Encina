```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.90GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.9798 ns | 0.1084 ns | 0.0059 ns |  0.14 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 6.9653 ns | 2.2000 ns | 0.1206 ns |  1.00 |    0.02 | 0.0003 |      24 B |        1.00 |
| FromBytes        | 7.6862 ns | 3.2236 ns | 0.1767 ns |  1.10 |    0.03 | 0.0003 |      24 B |        1.00 |
| ToBytes          | 7.4242 ns | 0.9679 ns | 0.0531 ns |  1.07 |    0.02 | 0.0004 |      32 B |        1.33 |
