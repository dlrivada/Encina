```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.13GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     38.57 ns |     0.218 ns |   0.012 ns |     1.24 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     31.14 ns |     5.636 ns |   0.309 ns |     1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 89,441.99 ns | 9,938.620 ns | 544.769 ns | 2,872.77 |   28.86 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    140.47 ns |     5.519 ns |   0.302 ns |     4.51 |    0.04 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     86.25 ns |    10.802 ns |   0.592 ns |     2.77 |    0.03 | 0.0105 |      - |     176 B |        1.57 |
