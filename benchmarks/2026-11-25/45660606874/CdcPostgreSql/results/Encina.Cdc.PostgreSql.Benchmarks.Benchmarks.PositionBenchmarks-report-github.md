```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 4.20GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.6719 ns | 1.7919 ns | 0.0982 ns |  0.12 |    0.02 |      - |         - |        0.00 |
| CreatePosition   | 5.4693 ns | 1.2926 ns | 0.0709 ns |  1.00 |    0.02 | 0.0003 |      24 B |        1.00 |
| FromBytes        | 5.3608 ns | 0.6340 ns | 0.0347 ns |  0.98 |    0.01 | 0.0003 |      24 B |        1.00 |
| ToBytes          | 7.5304 ns | 3.5137 ns | 0.1926 ns |  1.38 |    0.03 | 0.0004 |      32 B |        1.33 |
