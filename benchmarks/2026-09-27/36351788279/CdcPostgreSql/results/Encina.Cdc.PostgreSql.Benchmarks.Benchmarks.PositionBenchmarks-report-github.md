```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.55GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.8336 ns | 0.0274 ns | 0.0015 ns |  0.17 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 4.9891 ns | 0.3527 ns | 0.0193 ns |  1.00 |    0.00 | 0.0003 |      24 B |        1.00 |
| FromBytes        | 5.6056 ns | 0.9011 ns | 0.0494 ns |  1.12 |    0.01 | 0.0003 |      24 B |        1.00 |
| ToBytes          | 5.7230 ns | 1.5443 ns | 0.0846 ns |  1.15 |    0.02 | 0.0004 |      32 B |        1.33 |
