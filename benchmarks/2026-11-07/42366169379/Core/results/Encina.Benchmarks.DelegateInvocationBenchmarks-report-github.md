```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error         | StdDev       | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|--------------:|-------------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     38.96 ns |     16.719 ns |     0.916 ns |     1.25 |    0.03 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     31.20 ns |      8.986 ns |     0.493 ns |     1.00 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 91,403.01 ns | 21,960.810 ns | 1,203.746 ns | 2,929.82 |   52.44 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    131.28 ns |     37.961 ns |     2.081 ns |     4.21 |    0.08 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     81.24 ns |      9.427 ns |     0.517 ns |     2.60 |    0.04 | 0.0105 |      - |     176 B |        1.57 |
