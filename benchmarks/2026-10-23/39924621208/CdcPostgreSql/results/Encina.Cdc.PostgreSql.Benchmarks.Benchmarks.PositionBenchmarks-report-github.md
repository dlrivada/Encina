```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| ComparePositions | 0.8776 ns | 0.4918 ns | 0.0270 ns |  0.13 |      - |         - |        0.00 |
| CreatePosition   | 6.5704 ns | 0.5990 ns | 0.0328 ns |  1.00 | 0.0003 |      24 B |        1.00 |
| FromBytes        | 7.5943 ns | 1.8540 ns | 0.1016 ns |  1.16 | 0.0003 |      24 B |        1.00 |
| ToBytes          | 7.5611 ns | 0.3550 ns | 0.0195 ns |  1.15 | 0.0004 |      32 B |        1.33 |
