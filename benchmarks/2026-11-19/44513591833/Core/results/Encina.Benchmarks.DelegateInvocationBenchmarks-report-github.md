```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     28.66 ns |     5.943 ns |   0.326 ns |     1.31 |    0.01 | 0.0013 |      - |     112 B |        1.00 |
| DirectCall              |     21.83 ns |     0.682 ns |   0.037 ns |     1.00 |    0.00 | 0.0013 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 23,443.62 ns | 3,429.489 ns | 187.982 ns | 1,073.89 |    7.63 | 0.0610 | 0.0305 |    5287 B |       47.21 |
| GenericTypeConstruction |     97.76 ns |     6.680 ns |   0.366 ns |     4.48 |    0.02 | 0.0020 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     63.11 ns |     7.787 ns |   0.427 ns |     2.89 |    0.02 | 0.0020 |      - |     176 B |        1.57 |
