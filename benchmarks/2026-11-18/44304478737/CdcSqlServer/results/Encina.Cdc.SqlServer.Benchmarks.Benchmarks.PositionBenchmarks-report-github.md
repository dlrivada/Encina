```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.78GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| ComparePositions | 0.6915 ns | 0.1484 ns | 0.0081 ns |  0.09 |      - |         - |        0.00 |
| CreatePosition   | 7.4896 ns | 0.6179 ns | 0.0339 ns |  1.00 | 0.0003 |      24 B |        1.00 |
| FromBytes        | 7.5059 ns | 1.1302 ns | 0.0619 ns |  1.00 | 0.0003 |      24 B |        1.00 |
| ToBytes          | 6.6924 ns | 1.8517 ns | 0.1015 ns |  0.89 | 0.0004 |      32 B |        1.33 |
