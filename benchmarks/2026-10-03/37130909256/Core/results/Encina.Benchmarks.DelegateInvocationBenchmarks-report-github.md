```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     33.19 ns |    10.103 ns |   0.554 ns |     1.31 |    0.02 | 0.0013 |      - |     112 B |        1.00 |
| DirectCall              |     25.29 ns |     3.134 ns |   0.172 ns |     1.00 |    0.01 | 0.0013 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 27,375.88 ns | 1,932.853 ns | 105.946 ns | 1,082.45 |    7.31 | 0.0610 | 0.0305 |    5287 B |       47.21 |
| GenericTypeConstruction |    106.23 ns |    35.149 ns |   1.927 ns |     4.20 |    0.07 | 0.0020 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     66.31 ns |    49.414 ns |   2.709 ns |     2.62 |    0.09 | 0.0020 |      - |     176 B |        1.57 |
