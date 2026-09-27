```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.6309 ns |  0.0158 ns | 0.0009 ns |  0.10 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 6.0890 ns | 12.1406 ns | 0.6655 ns |  1.01 |    0.14 | 0.0003 |      24 B |        1.00 |
| FromBytes        | 6.5857 ns |  6.5530 ns | 0.3592 ns |  1.09 |    0.12 | 0.0003 |      24 B |        1.00 |
| ToBytes          | 5.6248 ns |  2.4462 ns | 0.1341 ns |  0.93 |    0.09 | 0.0004 |      32 B |        1.33 |
