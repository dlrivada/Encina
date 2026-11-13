```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.78GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| ComparePositions | 0.9421 ns | 0.1735 ns | 0.0095 ns |  0.14 |      - |         - |        0.00 |
| CreatePosition   | 6.5874 ns | 0.6852 ns | 0.0376 ns |  1.00 | 0.0010 |      24 B |        1.00 |
| FromBytes        | 7.4383 ns | 1.0422 ns | 0.0571 ns |  1.13 | 0.0010 |      24 B |        1.00 |
| ToBytes          | 8.2948 ns | 0.6333 ns | 0.0347 ns |  1.26 | 0.0013 |      32 B |        1.33 |
