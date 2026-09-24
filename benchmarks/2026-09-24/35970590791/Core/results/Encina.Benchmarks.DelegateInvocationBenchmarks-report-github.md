```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     37.06 ns |    14.409 ns |   0.790 ns |     1.24 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     29.95 ns |     5.052 ns |   0.277 ns |     1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 89,945.12 ns | 5,179.202 ns | 283.890 ns | 3,003.75 |   25.40 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    141.61 ns |    12.002 ns |   0.658 ns |     4.73 |    0.04 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     76.85 ns |     2.999 ns |   0.164 ns |     2.57 |    0.02 | 0.0105 |      - |     176 B |        1.57 |
