```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.96GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| ComparePositions | 0.9783 ns | 0.0619 ns | 0.0034 ns |  0.14 |      - |         - |        0.00 |
| CreatePosition   | 7.1039 ns | 0.5669 ns | 0.0311 ns |  1.00 | 0.0003 |      24 B |        1.00 |
| FromBytes        | 7.9168 ns | 1.3426 ns | 0.0736 ns |  1.11 | 0.0003 |      24 B |        1.00 |
| ToBytes          | 7.5274 ns | 1.0480 ns | 0.0574 ns |  1.06 | 0.0004 |      32 B |        1.33 |
