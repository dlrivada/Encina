```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     35.05 ns |     1.933 ns |   0.106 ns |     1.25 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     28.03 ns |     5.629 ns |   0.309 ns |     1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 66,371.83 ns | 4,549.009 ns | 249.347 ns | 2,367.97 |   23.72 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    128.93 ns |    13.344 ns |   0.731 ns |     4.60 |    0.05 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     92.19 ns |    27.493 ns |   1.507 ns |     3.29 |    0.06 | 0.0105 |      - |     176 B |        1.57 |
