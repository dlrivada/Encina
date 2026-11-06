```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.71GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     39.34 ns |     7.224 ns |   0.396 ns |     1.28 |    0.03 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     30.79 ns |    14.036 ns |   0.769 ns |     1.00 |    0.03 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 91,197.87 ns | 7,839.455 ns | 429.707 ns | 2,963.59 |   65.07 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    131.96 ns |     7.912 ns |   0.434 ns |     4.29 |    0.09 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     83.87 ns |    11.699 ns |   0.641 ns |     2.73 |    0.06 | 0.0105 |      - |     176 B |        1.57 |
