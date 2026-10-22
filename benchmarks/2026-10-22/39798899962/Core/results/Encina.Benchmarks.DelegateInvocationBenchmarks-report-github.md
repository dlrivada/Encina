```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     37.99 ns |     0.604 ns |   0.033 ns |     1.22 |    0.00 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     31.11 ns |     1.457 ns |   0.080 ns |     1.00 |    0.00 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 90,139.19 ns | 3,324.094 ns | 182.205 ns | 2,897.06 |    8.19 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    130.30 ns |     4.660 ns |   0.255 ns |     4.19 |    0.01 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     79.99 ns |     0.973 ns |   0.053 ns |     2.57 |    0.01 | 0.0105 |      - |     176 B |        1.57 |
