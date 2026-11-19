```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio  | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|-------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     31.02 ns |     5.140 ns |   0.282 ns |   1.18 |    0.01 | 0.0013 |      - |     112 B |        1.00 |
| DirectCall              |     26.21 ns |     3.970 ns |   0.218 ns |   1.00 |    0.01 | 0.0013 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 25,808.20 ns | 1,992.739 ns | 109.229 ns | 984.84 |    7.92 | 0.0610 | 0.0305 |    5287 B |       47.21 |
| GenericTypeConstruction |     99.50 ns |     4.326 ns |   0.237 ns |   3.80 |    0.03 | 0.0020 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     73.86 ns |    27.523 ns |   1.509 ns |   2.82 |    0.05 | 0.0020 |      - |     176 B |        1.57 |
