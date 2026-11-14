```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 23.678 ns | 6.1984 ns | 0.3398 ns |  0.80 | 0.0054 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.456 ns | 0.2128 ns | 0.0117 ns |  0.05 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 13.262 ns | 1.7322 ns | 0.0949 ns |  0.45 | 0.0022 |      56 B |        1.00 |
| CreateChangeMetadata       | 29.627 ns | 4.4717 ns | 0.2451 ns |  1.00 | 0.0022 |      56 B |        1.00 |
