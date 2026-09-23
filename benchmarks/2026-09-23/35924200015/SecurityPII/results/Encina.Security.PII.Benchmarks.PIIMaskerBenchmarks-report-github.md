```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                   | Job        | IterationCount | LaunchCount | Mean        | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------- |--------------- |------------ |------------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| Mask_SSN                 | Job-YFEFPZ | 10             | Default     |   449.33 ns |  18.238 ns | 12.063 ns |   4.93 |    0.14 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | Job-YFEFPZ | 10             | Default     |   533.42 ns |   1.935 ns |  1.280 ns |   5.85 |    0.08 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | Job-YFEFPZ | 10             | Default     | 2,483.51 ns |   9.978 ns |  5.938 ns |  27.22 |    0.36 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | Job-YFEFPZ | 10             | Default     | 4,011.06 ns |  13.940 ns |  9.221 ns |  43.97 |    0.58 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | Job-YFEFPZ | 10             | Default     | 3,853.05 ns |  21.945 ns | 14.515 ns |  42.24 |    0.57 | 0.0992 |    1752 B |        7.82 |
| Mask_CreditCard          | Job-YFEFPZ | 10             | Default     |   505.14 ns |   1.835 ns |  1.214 ns |   5.54 |    0.07 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | Job-YFEFPZ | 10             | Default     |    91.24 ns |   1.873 ns |  1.239 ns |   1.00 |    0.02 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | Job-YFEFPZ | 10             | Default     | 9,523.57 ns | 102.302 ns | 67.666 ns | 104.40 |    1.53 | 0.2899 |    5056 B |       22.57 |
| MaskObject_SingleField   | Job-YFEFPZ | 10             | Default     | 4,044.17 ns |  19.068 ns | 11.347 ns |  44.33 |    0.59 | 0.0992 |    1752 B |        7.82 |
| Mask_Phone               | Job-YFEFPZ | 10             | Default     |   427.66 ns |  14.860 ns |  9.829 ns |   4.69 |    0.12 | 0.0310 |     520 B |        2.32 |
|                          |            |                |             |             |            |           |        |         |        |           |             |
| Mask_SSN                 | ShortRun   | 3              | 1           |   406.77 ns |  26.785 ns |  1.468 ns |   4.49 |    0.04 | 0.0310 |     520 B |        2.32 |
| Mask_WithRegexPattern    | ShortRun   | 3              | 1           |   528.24 ns |   9.455 ns |  0.518 ns |   5.83 |    0.05 | 0.0248 |     416 B |        1.86 |
| MaskObject_NoAttributes  | ShortRun   | 3              | 1           | 2,478.77 ns |  34.771 ns |  1.906 ns |  27.35 |    0.25 | 0.0572 |    1008 B |        4.50 |
| MaskForAudit_SingleField | ShortRun   | 3              | 1           | 3,998.70 ns | 169.179 ns |  9.273 ns |  44.11 |    0.41 | 0.0992 |    1752 B |        7.82 |
| MaskForAudit_NonGeneric  | ShortRun   | 3              | 1           | 4,031.57 ns |  89.140 ns |  4.886 ns |  44.48 |    0.41 | 0.0992 |    1752 B |        7.82 |
| Mask_CreditCard          | ShortRun   | 3              | 1           |   523.98 ns | 188.109 ns | 10.311 ns |   5.78 |    0.11 | 0.0324 |     544 B |        2.43 |
| Mask_Email               | ShortRun   | 3              | 1           |    90.65 ns |  17.568 ns |  0.963 ns |   1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| MaskObject_MultiField    | ShortRun   | 3              | 1           | 9,276.60 ns | 295.356 ns | 16.189 ns | 102.34 |    0.95 | 0.2899 |    5056 B |       22.57 |
| MaskObject_SingleField   | ShortRun   | 3              | 1           | 4,024.17 ns | 480.085 ns | 26.315 ns |  44.40 |    0.48 | 0.0992 |    1752 B |        7.82 |
| Mask_Phone               | ShortRun   | 3              | 1           |   419.65 ns |  23.440 ns |  1.285 ns |   4.63 |    0.04 | 0.0310 |     520 B |        2.32 |
