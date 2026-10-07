```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.4578 ns | 0.5943 ns | 0.0326 ns |  0.14 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 3.2151 ns | 3.2042 ns | 0.1756 ns |  1.00 |    0.07 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 3.3389 ns | 2.7513 ns | 0.1508 ns |  1.04 |    0.06 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 3.3544 ns | 0.4532 ns | 0.0248 ns |  1.05 |    0.05 | 0.0019 |      32 B |        1.33 |
