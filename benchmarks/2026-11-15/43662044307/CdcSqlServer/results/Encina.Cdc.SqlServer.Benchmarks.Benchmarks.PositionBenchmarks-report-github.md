```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.25GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method           | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.6833 ns | 0.0015 ns | 0.0022 ns | 0.6828 ns |  0.14 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 4.9020 ns | 0.2377 ns | 0.3332 ns | 4.6721 ns |  1.00 |    0.09 | 0.0003 |      24 B |        1.00 |
| FromBytes        | 6.2914 ns | 0.1125 ns | 0.1684 ns | 6.2904 ns |  1.29 |    0.09 | 0.0003 |      24 B |        1.00 |
| ToBytes          | 5.4210 ns | 0.2820 ns | 0.4221 ns | 5.4766 ns |  1.11 |    0.11 | 0.0004 |      32 B |        1.33 |
