```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.63GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error         | StdDev       | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|--------------:|-------------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     38.49 ns |      3.780 ns |     0.207 ns |     1.30 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     29.61 ns |     10.820 ns |     0.593 ns |     1.00 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 88,552.25 ns | 30,142.601 ns | 1,652.218 ns | 2,991.13 |   71.35 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    130.01 ns |     17.116 ns |     0.938 ns |     4.39 |    0.08 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     79.84 ns |     17.963 ns |     0.985 ns |     2.70 |    0.06 | 0.0105 |      - |     176 B |        1.57 |
