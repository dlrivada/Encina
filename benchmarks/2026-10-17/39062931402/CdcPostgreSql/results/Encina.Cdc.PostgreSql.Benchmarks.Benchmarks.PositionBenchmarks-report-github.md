```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.7276 ns | 0.7329 ns | 0.0402 ns |  0.21 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 3.3979 ns | 2.1198 ns | 0.1162 ns |  1.00 |    0.04 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 3.5275 ns | 0.8432 ns | 0.0462 ns |  1.04 |    0.03 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 3.5475 ns | 4.4795 ns | 0.2455 ns |  1.04 |    0.07 | 0.0019 |      32 B |        1.33 |
