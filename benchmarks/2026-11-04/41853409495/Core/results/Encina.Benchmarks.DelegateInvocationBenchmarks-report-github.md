```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev    | Ratio    | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|----------:|---------:|--------:|-------:|----------:|------------:|
| CompiledDelegate        |     39.18 ns |     3.683 ns |  0.202 ns |     1.33 |    0.02 | 0.0044 |     112 B |        1.00 |
| DirectCall              |     29.56 ns |     8.409 ns |  0.461 ns |     1.00 |    0.02 | 0.0045 |     112 B |        1.00 |
| ExpressionCompilation   | 62,501.20 ns | 1,684.713 ns | 92.345 ns | 2,114.63 |   28.87 | 0.1221 |    5270 B |       47.05 |
| GenericTypeConstruction |    129.68 ns |    24.881 ns |  1.364 ns |     4.39 |    0.07 | 0.0069 |     176 B |        1.57 |
| MethodInfoInvoke        |     86.78 ns |    10.049 ns |  0.551 ns |     2.94 |    0.04 | 0.0069 |     176 B |        1.57 |
