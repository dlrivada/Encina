```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.18GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     36.69 ns |     1.197 ns |   0.066 ns |     1.24 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     29.60 ns |    11.455 ns |   0.628 ns |     1.00 |    0.03 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 91,245.94 ns | 6,739.787 ns | 369.430 ns | 3,083.63 |   57.33 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    134.82 ns |    43.999 ns |   2.412 ns |     4.56 |    0.11 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     82.10 ns |    17.692 ns |   0.970 ns |     2.77 |    0.06 | 0.0105 |      - |     176 B |        1.57 |
